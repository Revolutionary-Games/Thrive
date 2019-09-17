#pragma once

namespace thrive { namespace autoevo {

class RunResults;

//! \brief Base class for run steps
class RunStep {
public:
    virtual ~RunStep() = default;

    //! \brief Performs a single step. This needs to be called getTotalSteps
    //! count \returns True once final step is complete
    virtual bool
        step(RunResults& resultsStore) = 0;

    //! \returns Total number of steps
    //! \note As step is called this is allowed to return lower values than
    //! initially
    virtual int
        getTotalSteps() const = 0;
};

}} // namespace thrive::autoevo
